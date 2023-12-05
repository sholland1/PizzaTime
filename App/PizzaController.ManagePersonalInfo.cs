using Hollandsoft.OrderPizza;

namespace Controllers;
public partial class PizzaController {
    public PersonalInfo CreatePersonalInfo() {
        TerminalUI.PrintLine("Please enter your personal information.");
        var firstName = TerminalUI.Prompt("First name: ") ?? "";
        var lastName = TerminalUI.Prompt("Last name: ") ?? "";
        var email = TerminalUI.Prompt("Email: ") ?? "";
        var phone = TerminalUI.Prompt("Phone: ") ?? "";

        return ValidatePersonalAndSave(firstName, lastName, email, phone) ?? CreatePersonalInfo();
    }

    public PersonalInfo ManagePersonalInfo() {
        var currentInfo = Repo.GetPersonalInfo();
        if (currentInfo is null) {
            return CreatePersonalInfo();
        }

        TerminalUI.PrintLine("Edit your personal information:");
        var firstName = TerminalUI.PromptForEdit("First Name: ", currentInfo.FirstName) ?? "";
        var lastName = TerminalUI.PromptForEdit("Last Name: ", currentInfo.LastName) ?? "";
        var email = TerminalUI.PromptForEdit("Email: ", currentInfo.Email) ?? "";
        var phone = TerminalUI.PromptForEdit("Phone Number: ", currentInfo.Phone) ?? "";

        return ValidatePersonalAndSave(firstName, lastName, email, phone) ?? ManagePersonalInfo();
    }

    private PersonalInfo? ValidatePersonalAndSave(string firstName, string lastName, string email, string phone) {
        var personalInfo = new UnvalidatedPersonalInfo {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone
        }.Parse();

        TerminalUI.Clear();
        return personalInfo.Match<PersonalInfo?>(es => {
            TerminalUI.PrintLine("Failed to parse personal info:");
            TerminalUI.PrintLine(string.Join(Environment.NewLine, es.Select(e => e.ErrorMessage)));
            return default;
        }, pi => {
            Repo.SavePersonalInfo(pi);
            TerminalUI.PrintLine("Personal info saved.");
            return pi;
        });
    }
}
