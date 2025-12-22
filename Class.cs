using RRCServices.Clock;

namespace RRCApp
{
    public sealed class SystemClock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}
