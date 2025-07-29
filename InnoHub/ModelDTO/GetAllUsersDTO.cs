namespace InnoHub.ModelDTO
{
    public class GetAllUsersVM
    {
        public int Index { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string PhoneNumber { get; set; }
        public string RegisteredAt { get; set; }
        public bool IsBlocked { get; set; }
    }
}
