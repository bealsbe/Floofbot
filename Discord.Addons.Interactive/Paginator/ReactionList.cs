namespace Discord.Addons.Interactive
{
    /// <summary>
    /// The reaction list.
    /// </summary>
    public class ReactionList
    {
        public ReactionList()
        {
            First = false;
            Last = false;
            Forward = true;
            Backward = true;
            Jump = false;
            Trash = true;
            Info = false;
        }

        public bool First { get; set; }
        public bool Last { get; set; }
        public bool Forward { get; set; }
        public bool Backward { get; set; }
        public bool Jump { get; set; }
        public bool Trash { get; set; }
        public bool Info { get; set; } 
    }
}