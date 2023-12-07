# It's Pizza Time! 🍕

This application allows ordering Domino's pizzas from the command line. You can manage orders, payments, pizzas, and personal info. You can track your
order. The application uses OpenAI to design a pizza using natural language.

## Example usage

### Get an API key from OpenAI
You will need to set up an account with [OpenAI](https://openai.com/). Once you've created the key, it will be located here: https://platform.openai.com/api-keys. Copy it into your clipboard for the next step.

### Set the environment variable

#### Windows
Use the following command to set the environment variable. Set the key in the Environment Variables settings dialog to make it permanent.
```cmd
> set OPENAI_API_KEY=<your-key>
```

#### Linux
Use the following command to set the environment variable. Put this in your .bashrc or another initialization script to make it permanent.
```bash
$ export OPENAI_API_KEY='<your-key>'
```

Go to [OpenAI](https://help.openai.com/en/articles/5112595-best-practices-for-api-key-safety) for more information.

### Run the application
Run the program with no arguments to get started:
```bash
$ pizza-time
```

You will be presented with a menu where you can create and manage orders, pizzas, payments, and personal info. You can also track your order and view your order history.
```
It's Pizza Time!🍕
1. Place order
2. Manage orders
3. Manage pizzas
4. Manage payments
5. Edit personal info
6. Track order
7. View order history
q. Exit
```

Once you have created an order, you can place it with one of the following commands:
```bash
$ pizza-time --default-order
$ pizza-time --order <order-name>
```

Don't worry. You will be asked to confirm the order before it is placed.

You can track your order using the following command:

```bash
$ pizza-time --track
```

Additional commands:
```bash
$ pizza-time --version
$ pizza-time --help
```

## Build
Run the following in the solution directory:
```bash
$ dotnet build
```

## Test
Run the following in the solution directory:
```bash
$ dotnet test
```
