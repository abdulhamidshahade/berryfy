using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Berryfy.Domain.Entities
{
    public class InfrastructureResponse<T>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public T Value { get; set; }
    }
}
