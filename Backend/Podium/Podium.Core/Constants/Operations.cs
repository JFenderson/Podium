using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Core.Constants
{
    public static class Operations
    {

        public const string CreateOperation = "CreateOperation";
        public const string UpdateOperation = "UpdateOperation";
        public const string DeleteOperation = "DeleteOperation";
        public const string ReadOperation = "ReadOperation";
        



        public static string[] GetAllOperations()
        {
            return new[]
            {
                CreateOperation,
                UpdateOperation,
                DeleteOperation,
                ReadOperation
            };
        }
    }
}
