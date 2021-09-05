using BusinessCardTeamsExtension.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessCardTeamsExtension.Services
{
    public interface IBusinessCardService
    {
        Task<GetBusinessCardResponse> GetUserBusinessCard(string userId);
        Task<GetUserIdResponse> GetUserId(string email);
    }
}
