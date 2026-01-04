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
      await login(email, password);
    } else {
      throw Exception('Registration failed: ${response.statusCode} - ${response.body}');
    }
  }

  Future<void> changePassword(String oldPassword, String newPassword) async {
    if (_currentUser == null) return;
    
    final response = await http.post(
      Uri.parse('$baseUrl/customers/password'),
      headers: _headers,
      body: jsonEncode({
        'CustomerId': _currentUser!.id,
        'OldPassword': oldPassword,
        'NewPassword': newPassword,
      }),
    );

    if (response.statusCode != 204 && response.statusCode != 200) {
      throw Exception('Failed to change password: ${response.statusCode} - ${response.body}');
    }
  }

  Future<void> deleteAccount() async {
    if (_currentUser == null) return;

    final response = await http.delete(
      Uri.parse('$baseUrl/customers/${_currentUser!.id}'),
      headers: _headers,
    );

    if (response.statusCode == 204 || response.statusCode == 200) {
      _currentUser = null;
      notifyListeners();
    } else {
      throw Exception('Failed to delete account: ${response.statusCode} - ${response.body}');
    }
  }

  Future<void> logout() async {
    _currentUser = null;
    notifyListeners();
  }
}
