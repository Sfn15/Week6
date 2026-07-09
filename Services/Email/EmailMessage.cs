namespace Week6.Services.Email;

public enum EmailTemplate {OrderConfirmation, PasswordReset, WelcomeMessage}
public enum EmailStatus { Queued, Sent, Failed, Abandoned}

public record EmailMessage(
    Guid Id,
    string ToAddress,
    EmailTemplate Template,
    Dictionary<string,string> TemplateData,
    int AttemptCount = 0);

public record EmailDeliveryRecord(Guid Id, string ToAddress, EmailStatus Status, string? LastError, DateTime UpdatedAt);

