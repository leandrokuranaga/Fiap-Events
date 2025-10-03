namespace Fiap.Events.Services.Events.Models
{
    public class CreateAccount
    {
        public string Name { get; set; }
        public Email Email { get; set; }
        public int TypeUser { get; set; }
        public bool Active { get; set; }
        public List<object> LibraryGames { get; set; } = [];
        public int Id { get; set; }
    }
    public class Email
    {
        public string Address { get; set; }
    }
}
