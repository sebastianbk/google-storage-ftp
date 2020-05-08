using System;
using System.Net.Http;
using System.Threading.Tasks;
using FubarDev.FtpServer.AccountManagement;

namespace GoogleStorageFtp
{
    public class CustomMembershipProvider : IMembershipProvider
    {
        public async Task<MemberValidationResult> ValidateUserAsync(string name, string password)
        {
            // TODO: Implement a real authentication mechanism
            var authenticated = await Task.Run(() => name == Environment.GetEnvironmentVariable("USERNAME") && password == Environment.GetEnvironmentVariable("PWD"));

            if (authenticated)
            {
                return new MemberValidationResult(MemberValidationStatus.AuthenticatedUser, new CustomFtpUser(name));
            }
            else
            {
                return new MemberValidationResult(MemberValidationStatus.InvalidLogin);
            }
        }
    }

    internal class CustomFtpUser : IFtpUser
    {
        private string _name;

        public string Name
        {
            get => _name;
        }

        public CustomFtpUser(string name)
        {
            _name = name;
        }

        public bool IsInGroup(string groupName)
        {
            return groupName == "user" || groupName == Name;
        }
    }
}