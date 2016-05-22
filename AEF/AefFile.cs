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

    #region oldAef

/*public struct AefFile
{
    public AefFile(int tempo, IEnumerable<Instrument> instruments)
    {
        Tempo = tempo;
        Instruments = instruments.ToList();
    }

    public int Tempo { get; set; }
    public List<Instrument> Instruments { get; set; }

    public static AefFile Parse(string path)
    {
        var deserializer = new Deserializer(namingConvention:new CamelCaseNamingConvention());
        var x = deserializer.Deserialize<YamlDocument>(new StringReader(File.ReadAllText(path)));
        return new AefFile();
    }

    public static AefFile ParseXML(string path)
    {
        var reader =  XmlReader.Create(new StringReader(File.ReadAllText(path)));
        reader.Read();
        var doc = new XmlDocument();
        doc.Load(reader);
        //var doc = XElement.Load(reader);

        var instruments = doc.GetElementsByTagName("Instrument").Cast<XmlNode>().Select(ParseInstrument).ToList();
        var tempo =  doc.GetElementsByTagName("Tempo").Cast<XmlNode>().First();
        return new AefFile(int.Parse(tempo.InnerText),instruments);
    }

    private static Instrument ParseInstrument(XmlNode xmlNode)
    {
        return new Instrument(
        xmlNode.Attributes["Name"].Value,
            xmlNode.ChildNodes.Cast<XmlNode>().Select(ParsePattern).ToList());
    }

    private static Pattern ParsePattern(XmlNode xmlNode)
    {
        var hitsPerBar = int.Parse(xmlNode.Attributes["HitsPerBar"].Value);
        switch (xmlNode.Attributes["Type"].Value)
        {
            case "Hit":
                return new Hit(int.Parse(xmlNode.Attributes["HitsOn"].Value)).WithHitsPerBar(hitsPerBar);
            case "Positional":
                return ParsePositionPattern(xmlNode).WithHitsPerBar(hitsPerBar);
            default:
                throw new NotImplementedException();
        }
    }

    private static Positional ParsePositionPattern(XmlNode xmlNode)
    {
        return new Positional(xmlNode.SelectNodes("LocationPattern").Cast<XmlNode>()
            .Select(x => int.Parse(x.InnerText)).ToList(), 
            int.Parse(xmlNode.Attributes["MovesOn"].Value));
    }
    }*/
#endregion
 
}
 
 
 
 