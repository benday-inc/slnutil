namespace Benday.SolutionUtil.UnitTests.SampleClassesForSerialization;

public class Person
{
    public Person()
    {
        
    }

    public Person(int personNumber)
    {
        Id = personNumber;
        FirstName = $"FirstName_{personNumber}";
        LastName = $"LastName_{personNumber}";
        FavoriteNumber = personNumber * 100;
    }

    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public bool IsAwesome { get; set; }
    public int FavoriteNumber { get; set; } = 0;

    private List<PersonAddress> _addresses = new List<PersonAddress>();
    public List<PersonAddress> Addresses
    {
        get => _addresses;
        set => _addresses = value;
    }

    public void AddAddress(int addressNumber)
    {
        _addresses.Add(new PersonAddress
        {
            Line1 = $"Line1_{addressNumber}",
            Line2 = $"Line2_{addressNumber}",
            City = $"City_{addressNumber}",
            State = $"State_{addressNumber}",
            PostalCode = $"PostalCode_{addressNumber}"
        });
    }
    
}
