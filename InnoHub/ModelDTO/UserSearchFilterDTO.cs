namespace InnoHub.ModelDTO
{
    public class UserSearchFilterDTO
    {
        public string? Name { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Role { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
