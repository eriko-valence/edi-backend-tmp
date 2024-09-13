namespace lib_edi.Models.Edi
{
    public class EdiJob
    {
        public EdiJob()
        {
            Logger = new EdiJobLogger();
            Emd = new EdiJobEmd();
        }
        public EdiJobLogger Logger { get; set; }
        public EdiJobEmd Emd { get; set; }
    }
}
