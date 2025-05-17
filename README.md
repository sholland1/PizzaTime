# It's Pizza Time! 🍕

This application allows ordering Domino's pizzas from the command line. You can manage orders, payments, pizzas, and personal info. You can track your
order. The application uses OpenAI to design a pizza using natural language.

This application should work on any platform but has only been tested on Linux.

Warning: Domino's now requires solving a CAPTCHA to place an order. I haven't had time to implement a solution for this.

## Example usage

### Get an API key from OpenAI or Anthropic
You will need to set up an account an obtain an api key from [OpenAI](https://openai.com/) or [Anthropic](https://www.anthropic.com/).

### Set the environment variable

#### Linux
Use the following command to set the environment variable. Put this in your .bashrc or another initialization script to make it permanent.
```sh
$ export OPENAI_API_KEY='<your-key>'
$ export ANTHROPIC_API_KEY='<your-key>'
```

#### Windows
Use the following command to set the environment variable. Set the key in the Environment Variables settings dialog to make it permanent.
```bat
> set OPENAI_API_KEY=<your-key>
> set ANTHROPIC_API_KEY=<your-key>
```

### Run the application
Run the program with no arguments to get started:
```sh
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
```sh
$ pizza-time --default-order
$ pizza-time --order <order-name>
```

Don't worry. You will be asked to confirm the order before it is placed.

You can track your order using the following command:

```sh
$ pizza-time --track
```

You can also place an order and track it immediately with the following commands:
```sh
$ pizza-time --default-order --track
$ pizza-time --order <order-name> --track
```

Additional commands:
```sh
$ pizza-time --version
$ pizza-time --help
$ pizza-time --debug
```

## Build
Run the following in the solution directory:
```sh
$ dotnet build
```

## Test
Run the following in the solution directory:
```sh
$ dotnet test
```

## Install
Run the following in the solution directory on Linux:
```sh
$ ./install
```
