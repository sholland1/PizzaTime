using Hollandsoft.OrderPizza;

namespace Controllers;
public partial class PizzaController {
    public PersonalInfo CreatePersonalInfo() {
        _terminalUI.PrintLine("Please enter your personal information.");
        var firstName = _terminalUI.Prompt("First name: ") ?? "";
        var lastName = _terminalUI.Prompt("Last name: ") ?? "";
        var email = _terminalUI.Prompt("Email: ") ?? "";
        var phone = _terminalUI.Prompt("Phone: ") ?? "";

        return ValidatePersonalAndSave(firstName, lastName, email, phone) ?? CreatePersonalInfo();
    }

    public PersonalInfo ManagePersonalInfo() {
        var currentInfo = _repo.GetPersonalInfo();
        if (currentInfo is null) {
            return CreatePersonalInfo();
        }

        _terminalUI.PrintLine("Edit your personal information:");
        var firstName = _terminalUI.PromptForEdit("First Name: ", currentInfo.FirstName) ?? "";
        var lastName = _terminalUI.PromptForEdit("Last Name: ", currentInfo.LastName) ?? "";
        var email = _terminalUI.PromptForEdit("Email: ", currentInfo.Email) ?? "";
        var phone = _terminalUI.PromptForEdit("Phone Number: ", currentInfo.Phone) ?? "";

        return ValidatePersonalAndSave(firstName, lastName, email, phone) ?? ManagePersonalInfo();
    }

    private PersonalInfo? ValidatePersonalAndSave(string firstName, string lastName, string email, string phone) {
        var personalInfo = new UnvalidatedPersonalInfo {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone
        }.Parse();

        return personalInfo.Match<PersonalInfo?>(es => {
            _terminalUI.PrintLine("Failed to parse personal info:");
            _terminalUI.PrintLine(string.Join(Environment.NewLine, es.Select(e => e.ErrorMessage)));
            return default;
        }, pi => {
            _repo.SavePersonalInfo(pi);
            _terminalUI.PrintLine("Personal info saved.");
            return pi;
        });
    }
}
