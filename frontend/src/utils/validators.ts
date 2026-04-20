/**
 * Frontend validation utilities
 * These validators match backend FluentValidation rules
 */

export interface ValidationError {
  field: string;
  message: string;
}

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const USERNAME_REGEX = /^[a-zA-Z0-9_-]+$/;
const PASSWORD_REGEX = /^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,}$/;

export const validateEmail = (email: string): string | null => {
  if (!email || !email.trim()) {
    return 'Email jest wymagany.';
  }
  if (!EMAIL_REGEX.test(email)) {
    return 'Podaj prawidłowy adres email.';
  }
  return null;
};

export const validateUsername = (username: string): string | null => {
  if (!username || !username.trim()) {
    return 'Nazwa użytkownika jest wymagana.';
  }
  if (username.length < 8 || username.length > 50) {
    return 'Nazwa użytkownika musi mieć od 8 do 50 znaków.';
  }
  if (!USERNAME_REGEX.test(username)) {
    return 'Nazwa użytkownika może zawierać tylko litery, cyfry, myślnik i podkreślenie.';
  }
  return null;
};

export const validatePassword = (password: string): string | null => {
  if (!password || !password.trim()) {
    return 'Hasło jest wymagane.';
  }
  if (password.length < 8 || password.length > 128) {
    return 'Hasło musi mieć od 8 do 128 znaków.';
  }
  if (!PASSWORD_REGEX.test(password)) {
    return (
      'Hasło musi zawierać co najmniej jedną wielką literę, jedną małą literę, ' +
      'jedną cyfrę i jeden znak specjalny (@$!%*?&).'
    );
  }
  return null;
};

export const validateUserNameOrEmail = (value: string): string | null => {
  if (!value || !value.trim()) {
    return 'Nazwa użytkownika lub email jest wymagana.';
  }

  if (value.includes('@')) {
    if (!EMAIL_REGEX.test(value)) {
      return 'Podaj prawidłowy adres email.';
    }
  } else {
    if (value.length < 8 || value.length > 50) {
      return 'Nazwa użytkownika musi mieć od 8 do 50 znaków.';
    }
    if (!USERNAME_REGEX.test(value)) {
      return 'Nazwa użytkownika może zawierać tylko litery, cyfry, myślnik i podkreślenie.';
    }
  }

  return null;
};

export const validateFirstName = (firstName: string): string | null => {
  if (!firstName || !firstName.trim()) {
    return 'Imię jest wymagane.';
  }
  if (firstName.length > 50) {
    return 'Imię nie może być dłuższe niż 50 znaków.';
  }
  return null;
};

export const validateLastName = (lastName: string): string | null => {
  if (!lastName || !lastName.trim()) {
    return 'Nazwisko jest wymagane.';
  }
  if (lastName.length > 50) {
    return 'Nazwisko nie może być dłuższe niż 50 znaków.';
  }
  return null;
};

export const validateBaseCurrency = (currency: string): string | null => {
  if (!currency || !currency.trim()) {
    return 'Waluta bazowa jest wymagana.';
  }
  const currencyRegex = /^[A-Z]{3}$/;
  if (!currencyRegex.test(currency)) {
    return 'Kod waluty musi mieć dokładnie 3 znaki (np. PLN, EUR, USD).';
  }
  return null;
};

/**
 * Validate all register form fields at once
 */
export const validateRegisterForm = (data: {
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  baseCurrency?: string;
}): ValidationError[] => {
  const errors: ValidationError[] = [];

  const usernameError = validateUsername(data.username);
  if (usernameError) errors.push({ field: 'username', message: usernameError });

  const emailError = validateEmail(data.email);
  if (emailError) errors.push({ field: 'email', message: emailError });

  const passwordError = validatePassword(data.password);
  if (passwordError) errors.push({ field: 'password', message: passwordError });

  const firstNameError = validateFirstName(data.firstName);
  if (firstNameError) errors.push({ field: 'firstName', message: firstNameError });

  const lastNameError = validateLastName(data.lastName);
  if (lastNameError) errors.push({ field: 'lastName', message: lastNameError });

  const baseCurrencyError = validateBaseCurrency(data.baseCurrency || 'PLN');
  if (baseCurrencyError) errors.push({ field: 'baseCurrency', message: baseCurrencyError });

  return errors;
};

export const validateLoginForm = (data: {
  userNameOrEmail: string;
  password: string;
}): ValidationError[] => {
  const errors: ValidationError[] = [];

  // Dla logowania sprawdzamy tylko czy pola nie są puste
  if (!data.userNameOrEmail || !data.userNameOrEmail.trim()) {
    errors.push({ field: 'userNameOrEmail', message: 'Nazwa użytkownika lub email jest wymagana.' });
  }

  if (!data.password || !data.password.trim()) {
    errors.push({ field: 'password', message: 'Hasło jest wymagane.' });
  }

  return errors;
};
