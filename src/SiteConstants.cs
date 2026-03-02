namespace Site;

public static class SiteConstants
{
    public static class FieldNames
    {
        public const string Personality = "personality";

        public const string Zodiac = "zodiac";

        public const string Genre = "genre";

        public const string Birthdate = "birthdate";

        public const string Name = "name";

        public const string PersonId = "personId";
    }

    public static class IndexAliases
    {
        public const string CustomIndexElasticsearch = "CustomIndexElasticsearch"; 

        public const string CustomPersonIndex = "CustomPersonIndex";
    }
}