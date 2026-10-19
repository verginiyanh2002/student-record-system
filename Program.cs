using System;

class InvalidWamException : Exception
{
    public InvalidWamException(double wam)
        : base($"WAM must be between 0 and 100, but was {wam}.") { }
}

// Listing 14 style: backing field, explicit property
class OldStudent
{
    private double _wam;
    public double Wam
    {
        get => _wam;
        set
        {
            if (value < 0 || value > 100)
                throw new InvalidWamException(value);
            _wam = value;
        }
    }
}

// Listing 15 style: C# 14 'field' keyword
class NewStudent    
{
    public double Wam
    {
        get;
        set => field = (value is >= 0 and <= 100)
            ? value
            : throw new InvalidWamException(value);
    }
}

class Program
{
    static void Main()
    {
        var old = new OldStudent();
        old.Wam = double.NaN;
        Console.WriteLine($"Listing 14 version accepted NaN: Wam = {old.Wam}");

        var modern = new NewStudent();
        try
        {
            modern.Wam = double.NaN;
            Console.WriteLine($"Listing 15 version accepted NaN: Wam = {modern.Wam}");
        }
        catch (InvalidWamException ex)
        {
            Console.WriteLine($"Listing 15 version rejected NaN: {ex.Message}");
        }
    }
}
