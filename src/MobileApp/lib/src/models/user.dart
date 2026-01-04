class User {
  final int id;
  final String email;
  final String username;
  final String firstName;
  final String lastName;
  final bool active;

  User({
    required this.id,
    required this.email,
    required this.username,
    required this.firstName,
    required this.lastName,
    required this.active,
  });

  factory User.fromJson(Map<String, dynamic> json) {
    return User(
      id: json['Id'] ?? 0,
      email: json['Email'] ?? '',
      username: json['Username'] ?? '',
      firstName: json['FirstName'] ?? '',
      lastName: json['LastName'] ?? '',
      active: json['Active'] ?? false,
    );
  }
}
