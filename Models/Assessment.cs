using System;

namespace Models
{
    public class Assessment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime ProcessedOn { get; set; } = DateTime.Now;
        public bool IsLeveraged { get; set; }
        public string DisqualificationReason { get; set; }
        public Evidence Evidence { get; set; }
        public bool Disqualified { get => !string.IsNullOrEmpty(DisqualificationReason); }

        public Assessment(string obligor)
        {
            Id = Guid.NewGuid();
            ProcessedOn = DateTime.Now;
            Evidence = new Evidence { Obligor = obligor };
        }
    }
}
