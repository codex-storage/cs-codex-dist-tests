using Discord;

namespace BiblioTech.Options
{
    public abstract class CommandOption
    {
        public CommandOption(string name, string description, ApplicationCommandOptionType type, bool isRequired)
        {
            Name = name;
            Description = description;
            Type = type;
            IsRequired = isRequired;

            if (Description.Length > 100) throw new Exception("Description for option " + name + " too long!");
        }

        public string Name { get; }
        public string Description { get; }
        public ApplicationCommandOptionType Type { get; }
        public bool IsRequired { get; }

        public virtual SlashCommandOptionBuilder Build()
        {
            return new SlashCommandOptionBuilder()
                .WithName(Name)
                .WithDescription(Description)
                .WithType(Type)
                .WithRequired(IsRequired);
        }
    }
}
