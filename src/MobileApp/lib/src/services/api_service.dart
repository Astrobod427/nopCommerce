import 'dart:convert';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import '../models/product.dart';
import '../models/category.dart';
import '../models/language.dart';
import '../models/order.dart';
import '../models/currency.dart';

class ApiService {
  static const String _storageKey = 'api_base_url';
  
  // URL loaded from .env initially, but can be updated
  static String baseUrl = dotenv.env['API_BASE_URL'] ?? 'http://localhost:5010/api/simple';
  
  // API Key loaded from .env
  static String get apiKey => dotenv.env['API_KEY'] ?? '';

  static int languageId = 0;

  final http.Client client;

  ApiService({http.Client? client}) : client = client ?? http.Client();

  static Future<void> loadBaseUrl() async {
    final prefs = await SharedPreferences.getInstance();
    final storedUrl = prefs.getString(_storageKey);
    if (storedUrl != null && storedUrl.isNotEmpty) {
      baseUrl = storedUrl;
    }
  }

  static Future<void> setBaseUrl(String url) async {
    if (url.isEmpty) return;
    // Remove trailing slash if present
    if (url.endsWith('/')) {
      url = url.substring(0, url.length - 1);
    }
    baseUrl = url;
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_storageKey, url);
  }

  Map<String, String> get _headers => {
    'Content-Type': 'application/json',
    'X-API-KEY': apiKey,
  };

  Future<void> createOrder(OrderRequest orderRequest) async {
    final response = await client.post(
      Uri.parse('$baseUrl/orders/create'),
      headers: _headers,
      body: jsonEncode(orderRequest.toJson()),
    );

    if (response.statusCode != 200) {
      throw Exception('Failed to create order: ${response.statusCode} - ${response.body}');
    }
  }

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

  Future<List<Currency>> getCurrencies() async {
    final response = await client.get(
      Uri.parse('$baseUrl/currencies'),
      headers: _headers,
    );

    if (response.statusCode == 200) {
      final List<dynamic> data = json.decode(response.body);
      return data.map((json) => Currency.fromJson(json)).toList();
    } else {
      throw Exception('Failed to load currencies: ${response.statusCode}');
    }
  }

  Future<List<Order>> getOrders(int customerId) async {
    final response = await client.get(
      Uri.parse('$baseUrl/orders?customerId=$customerId'),
      headers: _headers,
    );

    if (response.statusCode == 200) {
      final List<dynamic> data = json.decode(response.body);
      return data.map((json) => Order.fromJson(json)).toList();
    } else {
      throw Exception('Failed to load orders: ${response.statusCode}');
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