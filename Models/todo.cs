namespace ToDoApi.Models
{
    public class ToDo
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime DueDate { get; set; }
    }
}
