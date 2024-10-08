﻿using TicketSystem.Dtos.Account;
using TicketSystem.Models;

namespace TicketSystem.Mappers
{
     public static class AccountMappers
     {
          public static NewUserDto ToAccountDto(this AppUser userModel)
          {
               return new NewUserDto
               {
                    UserName = userModel.UserName,
                    FirstName = userModel.FirstName,
                    LastName = userModel.LastName,
                    Email = userModel.Email,
               };
          }
     }
}
