import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import '../models/user.dart';

class AuthService extends ChangeNotifier {
  User? _currentUser;
  
  User? get currentUser => _currentUser;
  bool get isLoggedIn => _currentUser != null;

  static String get baseUrl => dotenv.env['API_BASE_URL'] ?? 'http://localhost:5010/api/simple';
  static String get apiKey => dotenv.env['API_KEY'] ?? '';

  Map<String, String> get _headers => {
    'Content-Type': 'application/json',
    'X-API-KEY': apiKey,
  };

  Future<void> login(String email, String password) async {
    final response = await http.post(
      Uri.parse('$baseUrl/customers/login'),
      headers: _headers,
      body: jsonEncode({
        'Email': email,
        'Password': password,
      }),
    );

    if (response.statusCode == 200) {
      final userData = json.decode(response.body);
      _currentUser = User.fromJson(userData);
      notifyListeners();
    } else {
      throw Exception('Login failed: ${response.statusCode} - ${response.body}');
    }
  }

  Future<void> register({
    required String email,
    required String password,
    required String firstName,
    required String lastName,
    String? username,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl/customers'),
      headers: _headers,
      body: jsonEncode({
        'Email': email,
        'Password': password,
        'FirstName': firstName,
        'LastName': lastName,
        'Username': username ?? email,
        'Active': true,
      }),
    );

    if (response.statusCode == 201 || response.statusCode == 200) {
      // Auto-login after registration or handle based on API response
      // If the API returns the created user, we can set it.
      // Based on controller: CreatedAtAction(..., new { id = customer.Id }, customer.Id);
      // It returns the ID in body (or location). 
      // To be safe and consistent, we can just call login immediately or rely on the user to login.
      // BUT, let's try to auto-login for better UX.
      await login(email, password);
    } else {
      throw Exception('Registration failed: ${response.statusCode} - ${response.body}');
    }
  }

  Future<void> logout() async {
    _currentUser = null;
    notifyListeners();
  }
}
