public class RateSourceEntity
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!; // NBP, ECB

    public string Name { get; set; } = null!;
}