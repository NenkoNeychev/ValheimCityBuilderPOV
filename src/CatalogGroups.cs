namespace CityBuilderPOV
{
    public static class CatalogGroups
    {
        public static int Id(int kind) => 20000 + kind;
        public static bool Matches(int tag, int kind) => tag == -1 || tag == Id(kind);
        public static string Name(int kind) => kind == 1 ? "Town buildings" : kind == 6 ? "Town walls" : kind == 2 ? "Town roads & terrain" : "Valheim";
    }
}
