using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;

namespace Assets.AECE.AEF
{
    public struct AefFile
    {
        public int Tempo { get; set; }
        public List<Instrument> Instruments { get; set; }

        public static AefFile Parse(string path)
        {
            var deserializer = new Deserializer(namingConvention: new PascalCaseNamingConvention());
            return deserializer.Deserialize<AefFile>(new StringReader(File.ReadAllText(path)));
        }
    }

    public class Instrument
    {
        public string Name { get; set; }
        public List<Pattern> Patterns { get; set; }
    }

    public class Pattern
    {
        public string Type { get; set; }
        public int HitsPerBar { get; set; }
        public int BarsToRepeatAfter { get; set; }
        public List<int> OccoursOn { get; set; }
        public int Offset { get; set; }
        public List<Location> LocationPattern { get; set; }
        public bool ShouldRepeat => BarsToRepeatAfter > 0;
        public int TotalHits => HitsPerBar * BarsToRepeatAfter;

        public int GetLocation(int index)
        {
            Debug.Assert(Type == "Positional" && LocationPattern.Any());
            // Use the last specified value appropriate for our index
            return LocationPattern.Last(x => x.Index <= index).Value;
        }

        public int CrotchetTillFirstEvent()
        {
            switch (Type)
            {
                case "IntensityIncrease":
                case "Hit":
                    return OccoursOn.First() -1;
                case "Positional":
                    return LocationPattern.First().Index - 1;
                default:
                    throw new ArgumentException();
            }
        }

        public bool IndexIsLastHit(int index)
        {
            switch (Type)
            {
                case "IntensityIncrease":
                case "Hit":
                    return index == OccoursOn.Last();
                case "Positional":
                    return index == LocationPattern.Last().Index;
                default:
                    throw new ArgumentException();
            }
        }

        public int GetEventAfter(int currentEvent)
        {
            switch (Type)
            {
                case "IntensityIncrease":
                case "Hit":
                    var prevItemPos = OccoursOn.IndexOf(currentEvent);
                    var nextItem = OccoursOn[prevItemPos + 1];
                    return nextItem;
                case "Positional":
                    var prevItem2 = LocationPattern.Find(x => x.Index == currentEvent);
                    var prevItemPos2 = LocationPattern.IndexOf(prevItem2);
                    var nextItem2 = LocationPattern[prevItemPos2 + 1];
                    return nextItem2.Index;
                default:
                    throw new ArgumentException();
            }
        }
    }

    public class Location
    {
        public int Index { get; set; }
        public int Value { get; set; }
    }
}
 
 
 
 
