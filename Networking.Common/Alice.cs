namespace Networking.Common;

// split definition of class across multiple files using partial
public partial class Alice : Person
{
    // You can never change Alice's gender hence const field, not even when Alice is instantiated
    public const string Gender = "female";

    // moreover, every instance of Alice will share the same field, not unique to the instance itself (class attribute, static field)
    public static string Name = "Alice";

    // calculated at runtime and can't change anytime after
    public readonly DateTime Instantiated;

    // You can change her hair color
    public string HairColor;

    // constructor
    public Alice()
    {
        // default values 
        HairColor = "not yet assigned";
        Instantiated = DateTime.Now;
    }

    // method of existing fields that can be get and set while behaving like a field (also known as a property)
    public string FactAboutAlice
    {
        get
        {
            return $"Alice's hair color is {HairColor}, and she was instantiated on: {Instantiated}.";
        }

        set
        {
            switch (value.ToLower())
            {
                case string p when p.Contains("brown"):
                    HairColor = "brown";
                    break;
                default:
                    HairColor = "black";
                    break;
                    // case null:
                    //     throw new ArgumentNullException(paramName: nameof(HairColor));
            }
        } // set it to whatever fact you wish
    }
}