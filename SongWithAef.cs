using Assets.AECE.AEF;

namespace Assets.AECE
{
    public class SongWithAef
    {
        public readonly string Name;
        public string Mp3;
        public AefFile Aef;

        public SongWithAef(string mp3, string aef, string name)
        {
            Name = name;
            Mp3 = mp3;
            Aef = AefFile.Parse(aef);
        }
    }
}