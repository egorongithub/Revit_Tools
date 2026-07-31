using System.Xml.Serialization;

namespace SminexBimTools.Settings
{
    /// <summary>Откуда разрешено брать параметр.</summary>
    public enum ParameterSource
    {
        /// <summary>По общему порядку поиска (тип/экземпляр — как задано в настройках).</summary>
        Auto,

        /// <summary>Только из экземпляра.</summary>
        Instance,

        /// <summary>Только из типа.</summary>
        Type
    }

    /// <summary>Шаг общего порядка поиска.</summary>
    public enum SearchStage
    {
        Type,
        Instance,
        System
    }

    /// <summary>
    /// Правило поиска: имя параметра и место, откуда его разрешено читать.
    /// </summary>
    public class ParameterRule
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public ParameterSource Source { get; set; } = ParameterSource.Auto;

        public ParameterRule()
        {
        }

        public ParameterRule(string name, ParameterSource source = ParameterSource.Auto)
        {
            Name = name;
            Source = source;
        }
    }
}
