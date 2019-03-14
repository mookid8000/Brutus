# Brutus

Brute force password-guessing. :neckbeard:

The program is a .NET Core 2.2 app. 

It will as quickly as possible try as many passwords as it can, salting & SHA512-hashing them, in an attempt to guess the original password.

Input to the program:

* a salt
* a hashed password (as if they came from a leaked credentials database)

and then

* an alphabet (which characters to cycle through)
* from length (which password length to start cycling from)
* to length (which password length to end with)

