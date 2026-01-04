import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'api_service.dart';

class ThemeProvider extends ChangeNotifier {
  bool _isDarkMode = false;
  bool get isDarkMode => _isDarkMode;

  // Dynamic colors
  Color _primaryColor = const Color(0xFF4AB2F1); // Default
  Color _secondaryColor = const Color(0xFF3B464F); // Default
  String? _logoUrl;

  Color get primaryColor => _primaryColor;
  Color get secondaryColor => _secondaryColor;
  String? get logoUrl => _logoUrl;

  final ApiService _apiService = ApiService();

  ThemeProvider() {
    _loadFromPrefs();
    fetchThemeFromApi();
  }

  Future<void> fetchThemeFromApi() async {
    try {
      final themeData = await _apiService.getTheme();
      
      if (themeData['PrimaryColor'] != null) {
        _primaryColor = _hexToColor(themeData['PrimaryColor']);
      }
      if (themeData['SecondaryColor'] != null) {
        _secondaryColor = _hexToColor(themeData['SecondaryColor']);
      }
      _logoUrl = themeData['LogoUrl'];
      
      notifyListeners();
    } catch (e) {
      print("Error loading theme: $e");
    }
  }

  Color _hexToColor(String hex) {
    hex = hex.replaceAll('#', '');
    if (hex.length == 6) {
      hex = 'FF$hex';
    }
    return Color(int.parse(hex, radix: 16));
  }

  void toggleTheme() {
    _isDarkMode = !_isDarkMode;
    _saveToPrefs();
    notifyListeners();
  }

  _loadFromPrefs() async {
    final prefs = await SharedPreferences.getInstance();
    _isDarkMode = prefs.getBool('isDarkMode') ?? false;
    notifyListeners();
  }

  _saveToPrefs() async {
    final prefs = await SharedPreferences.getInstance();
    prefs.setBool('isDarkMode', _isDarkMode);
  }
}
