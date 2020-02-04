using System;
using System.Linq;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

using CloudBook.Data;
using CloudBook.API.Data;
using CloudBook.API.Data.Database;
using User = CloudBook.API.Data.Database.User;

namespace CloudBook.API.Controllers
{
    [Route("users"), Authorize]
    public class UserController : ControllerBase
    {
        readonly UserManager<User> UserManager;
        readonly SignInManager<User> SignInManger;
        readonly TokenConfig TokenConfig;
        readonly IdentityStore Database;

        public UserController(UserManager<User> userManager, SignInManager<User> signInManager, IOptions<Data.TokenConfig> tokenConfig, IdentityStore database)
        {
            UserManager = userManager;
            SignInManger = signInManager;
            TokenConfig = tokenConfig.Value;
            Database = database;
        }

        [HttpGet("signin"), AllowAnonymous]
        public async Task<IActionResult> SignIn(string usr, string psw)
        {
            try
            {
                var result = await SignInManger.PasswordSignInAsync(userName: usr, password: psw, isPersistent: false, lockoutOnFailure: false);

                if (result.Succeeded)
                    return Content(GenerateToken());
                else if (result.IsLockedOut)
                    return BadRequest("Your account is locked out");
                else
                    return BadRequest("Invalid login attempt");
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("signout")]
        public async Task<IActionResult> SignOut()
        {
            Task tsk = SignInManger.SignOutAsync();
            await tsk.ConfigureAwait(false);

            if (tsk.IsCompletedSuccessfully)
                return Ok();
            else
                return BadRequest(tsk.Exception.ToJson());
        }

        [HttpPost("register"), AllowAnonymous]
        public async Task<IActionResult> Regiser([FromBody] Registration form)
        {
            if (!ModelState.IsValid)
                return BadRequest("Error 1000: Bad data provided");

            var user = new User(ref form);

            // Generate Hashsed password
            user.PasswordHash = UserManager.PasswordHasher.HashPassword(user, form.Password);

            var result = await UserManager.CreateAsync(user);

            if (!result.Succeeded)
                return BadRequest(error: result.Errors.ToList().ToString());

            // Update security stamp. Update it everytime something happens with an user
            await UserManager.UpdateSecurityStampAsync(user);

            // Get token for confirming email. Without token, email cannot be confirmed
            string token_link = await UserManager.GenerateEmailConfirmationTokenAsync(user);

            // Make the token uri-friendly
            token_link = Uri.EscapeUriString(token_link);

            // Create absolute link to varify the email providing username and token
            token_link = $"http://207.148.16.163/api/v1/user/varify?usr={user.UserName}&token={token_link}";

            // Doesn't work yet. Keep it commented before building/running
            Task task = SendVerificationEmailAsync(user.Email, token_link);

            // Run it with timeout limit
            await task.ConfigureAwait(false);

            if (task.IsCompletedSuccessfully)
                return Ok();
            else
                return BadRequest("Failed to send the email");
        }

        // TODO: Mark user for deletion
        [HttpGet("delete")]
        public IActionResult Delete()
        {
            // Create a data-set that only keeps track of the users that need to be removed.
            // It will perform a series of checks to see if there are any debit and credit left for that user
            // Until and unless there are, the user will be removed at the end of the month
            return Ok();
        }

        [AllowAnonymous]
        [HttpGet("verify")]
        public async Task<IActionResult> VerifyEmail(string usr, string token)
        {
            try
            {
                User user = await UserManager.FindByNameAsync(usr);

                // Create a new security stamp. Stamps are updated each time a user makes changes
                await UserManager.UpdateSecurityStampAsync(user);

                // If user's email is already confirmed, return bad req
                if (user.EmailConfirmed)
                    return BadRequest($"User {user.UserName} is already verified");

                // Provide token and user data to confirm email.
                var result = await UserManager.ConfirmEmailAsync(user, token);

                if (result.Succeeded)
                    return Ok($"User {user.UserName} has been varified");
                else
                    return BadRequest(result.Errors.ToJson());
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        /*
        Gets all the users info to be in compliance with GDPR Privacy Law
         */
        [HttpGet("get")]
        public IActionResult GetUserInfo() => Ok();

        [HttpPatch("update/password")]
        public async Task<IActionResult> ChangePassword(string old, string @new)
        {
            var user_identity = await UserManager.GetUserAsync(User);

            // Create a new security stamp. Stamps are updated each time a user makes changes
            await UserManager.UpdateSecurityStampAsync(user_identity);

            await UserManager.ChangePasswordAsync(user_identity, old, @new);

            return Accepted();
        }

        [HttpPatch("update/username")]
        public async Task<IActionResult> ChangeUserName(string user_name)
        {
            // Get current User's Identity
            User user_identity = await UserManager.GetUserAsync(User);

            // Attempt to update the user name of current User's identity
            var result = await UserManager.SetUserNameAsync(user_identity, user_name);

            if (!result.Succeeded)
                return BadRequest($"Error: {result.Errors}");

            // Create a new security stamp. Stamps are updated each time a user makes changes
            await UserManager.UpdateSecurityStampAsync(user_identity);

            return Accepted();
        }

        [Authorize]
        [HttpPost("update")] // Update all information except username and password
        public async Task<IActionResult> UpdateUserDetails([FromBody] CloudBook.Data.User form)
        {
            if (!ModelState.IsValid)
                return BadRequest("Error 1000: Bad data provided");

            // Get Identity information of User
            User user = await UserManager.GetUserAsync(base.User);

            // Update user information
            user.Update(ref form);

            // Sync with database
            var result = await UserManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                // TODO: Add Error Log to file
                return BadRequest("There was an error while updating user information");
            }

            // Create a new security stamp. Stamps are updated each time a user makes changes
            await UserManager.UpdateSecurityStampAsync(user);

            return Ok();
        }

        [HttpGet("friends/get")] // DONE
        public async Task<IActionResult> GetAllFriends()
        {
            User user = await UserManager.GetUserAsync(base.User);

            // List of friends for users
            List<string> friends = new List<string>();

            friends.AddRange((from relation in Database.UserRelations
                              where relation.FromUser == user
                              select relation.ToUserId)
                              .ToArray());

            friends.AddRange((from relation in Database.UserRelations
                              where relation.ToUser == user
                              select relation.FromUserId)
                              .ToArray());

            // If there aren't any available friends, return bad request
            if (friends.Count != 0)
                return BadRequest($"User '{user.UserName}' has no friends added");

            // Will not sort the list here, better to sort it on client end

            return Content(friends.ToJson());
        }

        [HttpPost("friends/add")] // UNTESTED
        public async Task<IActionResult> AddFriend([FromBody]string username)
        {
            User user = await UserManager.GetUserAsync(User);

            User friend = await UserManager.FindByNameAsync(username);

            if (friend.Equals(null))
                return BadRequest($"{username} doesn't exist");

            // Find if there is already a relation established between users
            bool isFriend = (from relation in Database.UserRelations
                             where (relation.FromUserId == user.UserName && relation.ToUserId == username)
                             || (relation.ToUserId == user.UserName && relation.FromUserId == username)
                             select relation)
                             .Any();

            if (isFriend)
                return BadRequest("Users are already friends");

            // Find if there are any friends request between the users
            isFriend = (from request in Database.Requests
                        where (request.UserName == user.UserName && request.Target == friend.UserName)
                        || (request.UserName == friend.UserName && request.Target == user.UserName)
                        select request).Any();

            if (isFriend)
                return BadRequest($"A friend request between {user.UserName} and {friend.UserName} already exists");

            Database.Requests.Add(new Request() { Target = friend.UserName, User = user });

            // Add a new relation between the two users, with time of creation being NOW
            Database.UserRelations.Add(new UserRelation()
            {
                FromUser = user,
                ToUser = friend,
                Date = DateTime.UtcNow
            });

            await Database.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("friends/verify")]
        public IActionResult VerifyFriendsRequest(string name)
        {
            string username = UserManager.GetUserName(User);

            var query = from reqst in Database.Requests
                        where reqst.Target == username && reqst.UserName == name
                        select reqst;

            // If there are no request, return bad request
            if (!query.Any())
                return BadRequest("No such request exists");

            // Remove the request
            Database.Requests.Remove(query.Single());

            // Create new relation as request was varified
            var userRelation = new UserRelation()
            {
                ToUserId = username,
                FromUserId = name,
                Date = DateTime.UtcNow,
            };

            Database.UserRelations.Add(userRelation);

            Database.SaveChanges();

            return Ok();
        }

        // Show stats based on range provided
        [HttpGet("stats/expense")]
        public IActionResult GetExpenseStats(string orderby, byte range)
        {
            string username = UserManager.GetUserName(base.User);

            // Create a date vs amount dictonary to store stats
            Dictionary<DateTime, double> stats = new Dictionary<DateTime, double>();

            // Convert to lower letters for uniformity
            orderby = orderby.ToLower();

            byte limit;

            if (range <= 0)
                return BadRequest("'Range' cannot be 0 or less");
            else if (range > 10)
                return BadRequest("'Range' cannot be more than 10");

            switch (orderby)
            {
                case "week":
                    limit = 7;
                    range = (byte)(7 * range);
                    break;
                case "month":
                    limit = 30;
                    range = (byte)(30 * range);
                    break;
                case "day":
                    limit = 1;
                    break;
                default:
                    return BadRequest("Invalid parameter provided for 'OrderBy'. It has to be 'day'/'week'/'month'");
            }

            for (byte i = 0; i < range; i += limit)
            {
                DateTime date = DateTime.Today.AddDays(-i);
                double expense = (from log in Database.PurchaseLogs
                                  where log.Username == username &&
                                  log.isVerified == true &&
                                  log.Time <= date &&
                                  log.Time > date.AddDays(-i - limit)
                                  select log.Amount)
                                  .Sum();

                stats.Add(date, Math.Round(expense, 2, MidpointRounding.AwayFromZero));
            }

            if (!stats.Any())
                return NoContent();
            else
                return Content(stats.ToJson());
        }
        
        // Private method to generate JSON Web Token
        string GenerateToken(params Claim[] claims)
            => new JwtSecurityTokenHandler().CreateEncodedJwt(
                    issuer: TokenConfig.Issuer,
                    audience: TokenConfig.Audience,
                    subject: claims == null ? null : new ClaimsIdentity(claims), // If claim is null, set subject to null
                    notBefore: DateTime.UtcNow.AddSeconds(2),
                    expires: DateTime.UtcNow.AddDays(1),
                    issuedAt: DateTime.UtcNow,
                    signingCredentials: new SigningCredentials(
                        key: new SymmetricSecurityKey(key: TokenConfig.GetKey()),
                        algorithm: SecurityAlgorithms.HmacSha512
                    )
                );

        async Task SendVerificationEmailAsync(string toAddress, string link)
        {
            // Create new Mail message to user's email
            MailMessage mail = new MailMessage("no-reply@blueatlantis.com.bd", toAddress)
            {
                IsBodyHtml = true,
                Body = link,
                Subject = "Welcome to Blue Bean!",
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8
            };

            // Clear attachements if any
            if (mail.Attachments.Any())
                mail.Attachments.Clear();

            // Create new smtpclient connection
            SmtpClient client = new SmtpClient()
            {
                Port = 587,
                EnableSsl = true,
                DeliveryFormat = SmtpDeliveryFormat.International,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential(mail.From.Address, "{Password}"),
                Host = "smtp.gmail.com",
                Timeout = 3000
            };
            await client.SendMailAsync(mail);

            // Clear out resources
            client.Dispose();
            mail.Dispose();
        }
    }
}