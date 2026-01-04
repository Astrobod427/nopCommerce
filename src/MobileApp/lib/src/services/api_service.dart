import 'dart:convert';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:http/http.dart' as http;
import '../models/product.dart';
import '../models/category.dart';
import '../models/language.dart';

class ApiService {
  // URL loaded from .env
  static String get baseUrl => dotenv.env['API_BASE_URL'] ?? 'http://localhost:5010/api/simple';
  
  // API Key loaded from .env
  static String get apiKey => dotenv.env['API_KEY'] ?? '';

  static int languageId = 0;

  final http.Client client;

  ApiService({http.Client? client}) : client = client ?? http.Client();

  Map<String, String> get _headers => {
    'Content-Type': 'application/json',
    'X-API-KEY': apiKey,
  };

  Future<List<Product>> getProducts({int? categoryId}) async {
    String queryParams = '?languageId=$languageId';
    if (categoryId != null) {
      queryParams += '&categoryId=$categoryId';
    }
    
    final response = await client.get(
      Uri.parse('$baseUrl/products$queryParams'),
      headers: _headers,
    );

    if (response.statusCode == 200) {
      final List<dynamic> data = json.decode(response.body);
      return data.map((json) => Product.fromJson(json)).toList();
    } else {
      throw Exception('Failed to load products: ${response.statusCode}');
    }
  }

  Future<Product> getProductById(int id) async {
    final response = await client.get(
      Uri.parse('$baseUrl/products/$id'),
      headers: _headers,
    );

    if (response.statusCode == 200) {
      return Product.fromJson(json.decode(response.body));
    } else {
      throw Exception('Failed to load product details: ${response.statusCode}');
    }
  }

  Future<List<Category>> getCategories() async {
    final response = await client.get(
      Uri.parse('$baseUrl/categories'),
      headers: _headers,
    );

    if (response.statusCode == 200) {
      final List<dynamic> data = json.decode(response.body);
      return data.map((json) => Category.fromJson(json)).toList();
    } else {
      throw Exception('Failed to load categories: ${response.statusCode}');
    }
  }

  Future<List<Language>> getLanguages() async {
    final response = await client.get(
      Uri.parse('$baseUrl/languages'),
      headers: _headers,
    );

    if (response.statusCode == 200) {
      final List<dynamic> data = json.decode(response.body);
      return data.map((json) => Language.fromJson(json)).toList();
    } else {
      throw Exception('Failed to load languages: ${response.statusCode}');
    }
  }

  Future<Map<String, dynamic>> getTheme() async {
    final response = await client.get(
      Uri.parse('$baseUrl/theme'),
      headers: _headers,
    );

    if (response.statusCode == 200) {
      return json.decode(response.body);
    } else {
      throw Exception('Failed to load theme: ${response.statusCode}');
    }
  }
}