namespace LabTrack.Tests.Soporte
{
    // Las pruebas de esta colección comparten la base temporal y se ejecutan en serie.
    [CollectionDefinition(Nombre)]
    public sealed class ColeccionBaseDatos : ICollectionFixture<BaseDatosFixture>
    {
        public const string Nombre = "BaseDatos";
    }
}
