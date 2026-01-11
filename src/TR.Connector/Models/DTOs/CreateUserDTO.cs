using TR.Connector.Models.Entites;

namespace TR.Connector.Models.DTOs
{
    internal class CreateUserDTO : UserPropertyData
    {
        public string? password { get; set; }
    }
}
