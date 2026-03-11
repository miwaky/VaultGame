namespace ShelterCommand
{
    /// <summary>
    /// Enumerates all named rooms in the shelter.
    /// Index 0 (Dorm) is the default starting room for all survivors on day 1.
    /// </summary>
    public enum ShelterRoomType
    {
        Dorm       = 0,   // default spawn room
        Hospital   = 1,
        Factory    = 2,
        Restroom   = 3,
    }
}
