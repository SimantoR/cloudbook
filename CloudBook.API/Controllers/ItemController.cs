using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using CloudBook.API.Data.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CloudBook.API.Controllers
{
    [Route("users/items"), Authorize]
    public class ItemController : ControllerBase
    {
        private readonly UserManager<User> UserManager;
        private readonly IdentityStore Database;
        private readonly IConfiguration Configuration;
        private readonly string error_log_path = "errors.log";

        public ItemController(UserManager<User> manager, IdentityStore database, IConfiguration configuration)
        {
            UserManager = manager;
            Database = database;
            Configuration = configuration;
            error_log_path = Path.Combine(Environment.CurrentDirectory, "server_files", error_log_path);
        }

        [HttpGet] // Implemented
        public IActionResult GetItems(byte offset = 0b1, byte limit = 0b10100)
        {
            string username = UserManager.GetUserName(base.User);

            // Offset nee
            offset = offset == 0 ? (byte)0b1 : (byte)(offset - 0b1);

            PurchaseLog[] items = (
                from log in Database.PurchaseLogs
                where log.Username == username
                orderby log.Time
                select log
            ).Page(offset, limit).ToArray();

            if (items.Count() == 0)
                return BadRequest("No items found");
            else
                return Content(items.ToJson());
        }

        [HttpGet("get/{id}")]
        public IActionResult GetPurchaseLog(Guid id)    // Doesn't function. [Convert return data type]
        {
            try
            {
                var purchaseLog = Database.PurchaseLogs.Where(arg => arg.Id == id).Single().Pack();
                purchaseLog.Sharers = (
                    from arg in Database.LogRelations
                    where arg.PurchaseLog.Id == id
                    select arg.User.UserName
                ).ToArray();

                return Content(purchaseLog.ToJson());
            }
            catch (IOException)
            { return BadRequest(); }
        }

        [HttpPost("add")] // Implemented
        public async Task<IActionResult> AddItem([FromBody] CloudBook.Data.PurchaseLog model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data provided");

            string username = UserManager.GetUserName(this.User);

            PurchaseLog purchase_log = new PurchaseLog(ref model, UserManager.GetUserName(base.User));

            if (model.Sharers != null || model.Sharers.Length != 0)
            {
                purchase_log.isVerified = false;

                // Round up the splitted amount to three digit for proper final rounding (important). Used away from Zero rounding for statistical purposes
                double split_amount = model.Amount / (model.Sharers.Length + 1);
                split_amount = Math.Round(split_amount, 3, MidpointRounding.AwayFromZero);

                Database.LogRelations.Add(new LogRelation()
                {
                    Amount = split_amount,
                    User = await UserManager.GetUserAsync(User),
                    PurchaseLog = purchase_log
                });

                for (int i = 0; i < model.Sharers.Length; i++)
                {
                    // If the i-th friend and user are friends
                    bool are_friends = (from rel in Database.UserRelations
                                        where (rel.FromUserId == username && rel.ToUserId == model.Sharers[i]) ||
                                        (rel.ToUserId == username && rel.FromUserId == model.Sharers[i])
                                        select rel).Any();

                    if (!are_friends)
                        return BadRequest($"{username} and {model.Sharers[i]} are not friends");

                    // Its safe to add this here because if there is a implication that any of the list of 'friends'
                    // isn't actual friend of user, the requests will not be saved as Database.SaveChanges() is called at the bottom
                    Database.Requests.Add(new Request()
                    {
                        UserName = model.Sharers[i],
                        Target = purchase_log.Id.ToString(),
                        Data = split_amount.ToString()
                    });
                }
            }
            else
            {
                purchase_log.isVerified = true;
                Database.LogRelations.Add(new LogRelation()
                {
                    Amount = model.Amount,
                    User = await UserManager.GetUserAsync(User),
                    PurchaseLog = purchase_log
                });
            }

            try {
                // Move the receipt image from tmp to storage
                System.IO.File.Move($"/tmp/{model.ReceiptId}.jpg", $"{Environment.CurrentDirectory}/storage/{model.ReceiptId}.jpg");
            } catch { return BadRequest("Error occured. HTTPS Request canceled"); }

            // Save changes
            Database.SaveChanges();

            // Return the updated model to the client
            return Content(model.ToJson());
        }

        [HttpDelete("delete")]
        public IActionResult RemoveItem([FromBody] Guid id)
        {
            try
            {
                string username = UserManager.GetUserName(User);

                var query = from logs in Database.PurchaseLogs
                            where logs.Id == id && logs.Username == username
                            select logs;

                // If there are no such log, return bad request with an explanation
                if (!query.Any())
                    return BadRequest($"No such log exists: {id}");

                // If there is a long that exists, take it from database
                PurchaseLog log = query.Single();

                // Remove the log
                Database.PurchaseLogs.Remove(log);

                // Save changes
                Database.SaveChanges();

                // Return Accepted Response
                return Accepted();
            }
            catch (Exception ex)
            {
                CreateErrorLogAsync(ex).Wait();
                return BadRequest($"User or log doesn't exist");
            }
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyItem([FromBody] Guid id)
        {
            string username = UserManager.GetUserName(User);

            try
            {
                // Find the request to be varified
                var query = from arg in Database.Requests
                            where arg.UserName == username && arg.Target == Convert.ToString(id)
                            select arg;

                if (!query.Any()) // If there aren't any request
                    return BadRequest("No such request exists");
                else if (query.Count() > 1) // If there there are more than one instances of the same request
                    throw new Exception("There are multiple instances of same request");

                Request request = query.Single();

                // Create new log relation
                LogRelation relation = new LogRelation()
                {
                    PurchaseLog = (from arg in Database.PurchaseLogs where arg.Id == id select arg).Single(),
                    Amount = Convert.ToDouble(request.Data),
                    User = await UserManager.GetUserAsync(base.User)
                };

                // Add the log relation to database
                Database.LogRelations.Add(relation);

                // Remove the request as the request is being verified
                Database.Requests.Remove(request);

                Database.SaveChanges();

                query = from arg in Database.Requests where arg.Target == id.ToString() select arg;

                // If there are no more requests left
                if (!query.Any())
                {
                    // Find the itemlog
                    var itemlog = (from arg in Database.PurchaseLogs where arg.Id == id select arg).Single();

                    // Attach the itemlog to be tracked for changes
                    Database.PurchaseLogs.Attach(itemlog);

                    // Change the status of the itemlog to be varified
                    itemlog.isVerified = true;

                    Database.SaveChanges();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                CreateErrorLogAsync(ex).Wait();
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateItem(Guid id, [FromBody] CloudBook.Data.PurchaseLog model)
        {
            string username = UserManager.GetUserName(base.User);

            PurchaseLog purchaseLog = (
                from arg in Database.PurchaseLogs
                where arg.Id == id && arg.Username == username
                select arg)
                .Single();

            if (purchaseLog.Equals(null))
                return Unauthorized();

            purchaseLog.Update(ref model);
            Database.PurchaseLogs.Update(purchaseLog);

            try { await Database.SaveChangesAsync(true); }
            catch { return BadRequest(); }

            return Accepted();
        }

        [HttpGet("receipts/{id}")] // DONE
        public async Task<IActionResult> GetReceipt(Guid id, byte quality = 75, bool metadata = false)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid id provided. It has to be a valid GUID");

            // Upper-Clamp
            if (quality > 100 || quality == 0)
                quality = 100;

            string username = UserManager.GetUserName(User);

            // Get absolute path of the required receipt
            string path = Path.Combine(Directory.GetCurrentDirectory(), "server_files", $"{id}.jpg");

            // If the log receipt doesn't belong to the user, return
            if (!Database.PurchaseLogs.Where(x => x.Username == username && x.Id == id).Any())
                return Unauthorized();
            else if (!Database.LogRelations.Where(x => x.PurchaseLog.Id == id && x.PurchaseLog.Username == username).Any())
                return Unauthorized();

            // If the receipt doesn't exist return bad request stating the situation
            if (!System.IO.File.Exists(path))
                return BadRequest($"No such receipt exists");

            try
            {
                // Try to load the receipt image and return it as image
                MemoryStream ms = new MemoryStream();
                using (var image = SixLabors.ImageSharp.Image.Load(path))
                {
                    image.SaveAsJpeg(stream: ms, encoder: new JpegEncoder() {
                        Quality = quality, Subsample = JpegSubsample.Ratio420, IgnoreMetadata = !metadata
                    });

                    // Position has to be resetted after saving an image to memory stream
                    ms.Position = 0;
                }

                // Return the image as file
                return File(fileStream: ms, contentType: "image/jpeg", fileDownloadName: $"{id}.jpg");
            }
            catch (IOException ex)
            {
                await CreateErrorLogAsync(ex);
                return BadRequest("Error returning the receipt");
            }
        }

        // Privacy Policy:
        // We will be storing the receipt images for analysis
        // so that we can serve our users with important info
        // like categorized purchase trends. We will only keep
        // such data for an max period of 60 days which is what
        // we are deeming as safe amount of time for receipts
        // to be analyzed. After that we only hold the receipt
        // data tagged by user account 
        [HttpPost("receipts/add")]
        public IActionResult UploadReceipt([FromBody] IFormFile receipt)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data provided");

            if (receipt.ContentType != "image/jpeg" || receipt.ContentType != "image/png")
                return BadRequest("File is not an image");

            // A guid to uniquely identify the receipt
            Guid newId = Guid.NewGuid();

            string outputPath = $"{DateTime.UtcNow.Date}:{newId}";

            using (FileStream output = new FileStream($"/tmp/{outputPath}.jpg", FileMode.Create))
            {
                Image<Rgba32> receiptImg = Image.Load(receipt.OpenReadStream());

                receiptImg.Mutate(ctx => {
                    ctx.BlackWhite();
                    ctx.BinaryThreshold(0.5f);
                });

                //* Maybe use binarization as well?

                // Save to file using full qualitys
                receiptImg.Save(stream: output, encoder: new JpegEncoder()
                {
                    Subsample = JpegSubsample.Ratio444,
                    Quality = 100,
                    IgnoreMetadata = false
                });
            }
            // Return the reference Id of the receipt
            return Accepted(outputPath);
        }

        private async Task CreateErrorLogAsync(Exception ex)
        {
            // TODO: Use Mutex to create synchronized access to file.
            // See: https://stackoverflow.com/questions/10210264/writing-text-to-file-from-multiple-instances-of-program#10210458

            using (FileStream fileStream = new FileStream(error_log_path, FileMode.Append))
            using (StreamWriter writer = new StreamWriter(fileStream))
            {
                Data.ErrorLog errorLog = new Data.ErrorLog(ex);

                await writer.WriteAsync(errorLog.ToJson());
            }
        }
    }
}