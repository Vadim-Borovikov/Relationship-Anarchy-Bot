using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RelationshipAnarchyBot
{
    public class PersonInfo
    {
        public readonly string Username;
        public readonly List<Feature> Features;
        public readonly byte FeaturesVersion;

        public PersonInfo(string username, List<Feature> featuresTemplate, byte featuresVersion)
        {
            Username = username;
            Features = featuresTemplate.Select(f => f.Clone()).ToList();
            FeaturesVersion = featuresVersion;
        }

        public static string Encode(PersonInfo info)
        {
            List<byte> bytes = new List<byte> { info.FeaturesVersion };

            byte[] options = info.Features.Select(f => (byte)f.Selected).ToArray();
            bytes.AddRange(options);

            byte[] name = Encoding.ASCII.GetBytes(info.Username);
            bytes.AddRange(name);

            return Convert.ToBase64String(bytes.ToArray());
        }

        public static PersonInfo Decode(string code, byte currentVersion, List<Feature> featuresTemplate)
        {
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(code);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
            int index = 0;

            byte version = bytes[index];
            if (version != currentVersion)
            {
                return null;
            }
            ++index;

            List<Feature> features = featuresTemplate.Select(f => f.Clone()).ToList();
            for (int i = 0; i < features.Count; ++i, ++index)
            {
                features[i].Selected = (Selected) bytes[index];
            }

            string username = Encoding.ASCII.GetString(bytes, index, bytes.Length - index);

            return new PersonInfo(username, features, version);
        }

        public List<Feature> Match(PersonInfo info)
        {
            var matches = new List<Feature>();
            for (int i = 0; i < Features.Count; ++i)
            {
                Feature feature = Features[i];
                Feature partnerFeature = info.Features[i];

                if (feature.Selected != Selected.No && partnerFeature.Selected != Selected.No)
                {
                    matches.Add(feature);
                }
            }

            return matches;
        }
    }
}
