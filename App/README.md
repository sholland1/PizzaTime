# Order Pizza

This application allows ordering Domino's pizzas from the command line. You can manage orders, payments, pizzas, and personal info. You can track your
order. The application uses OpenAI to design a pizza using natural language.

## Example usage

First set the OpenAI API key:
```bash
$ order-pizza --set-api-key <api-key>
```

Then setup your personal info, pizzas, payments, and orders using the menus:
```bash
$ order-pizza
```
```
Welcome to the pizza ordering app!🍕
1. Place order
2. Manage orders
3. Manage pizzas
4. Manage payments
5. Edit personal info
6. Track order
7. View order history
q. Exit
```

You can order a pizza using the menu or by using one of the following commands:
```bash
$ order-pizza --default-order
$ order-pizza --order <named-order>
```

Don't worry. You will be asked to confirm the order before it is placed.

You can track your order using the following command:

```bash
$ order-pizza --track
```

Additional commands:
```bash
$ order-pizza --version
$ order-pizza --help
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