using BusinessCardTeamsExtension.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessCardTeamsExtension.Services
{
    public interface IMicrosoftGraphService
    {
        Task<ADUser> GetUser(string userId);
    }
}
