namespace Hollandsoft.PizzaTime;

public interface IDateGetter {
    DateTime GetDate();
    DateTime GetDateTime();
    DateOnly GetDateOnly();
}

public class CurrentDateGetter : IDateGetter {
    public DateTime GetDate() => DateTime.Today;
    public DateTime GetDateTime() => DateTime.Now;
    public DateOnly GetDateOnly() => DateOnly.FromDateTime(DateTime.Today);
}

public class FixedDateGetter(DateTime _date) : IDateGetter {
    public DateTime GetDate() => _date.Date;
    public DateTime GetDateTime() => _date;
    public DateOnly GetDateOnly() => DateOnly.FromDateTime(_date);
}
