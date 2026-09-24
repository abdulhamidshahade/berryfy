using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Berryfy.Application.Dtos.AuthDtos.Requests
{
    public class UpdateRoleRequest
    {
        public string oldRoleName { get; set; }
        public string newRoleName { get; set; }
    }
}
