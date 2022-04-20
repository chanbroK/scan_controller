namespace scan_controller.Models
{
    public class TwainTask
    {
        private int Id { get; set; }
        private string Name { get; set; }

        public int getId()
        {
            return Id;
        }

        public void setId(int id)
        {
            Id = id;
        }

        public string getName()
        {
            return Name;
        }

        public void setName(string name)
        {
            Name = name;
        }
    }
}