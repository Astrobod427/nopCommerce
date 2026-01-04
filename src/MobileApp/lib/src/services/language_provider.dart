import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/language.dart';
import 'api_service.dart';

class LanguageProvider extends ChangeNotifier {
  Language? _currentLanguage;
  int _currentLanguageId = 0;
  String _currentCulture = 'en-US';

  Language? get currentLanguage => _currentLanguage;
  int get currentLanguageId => _currentLanguageId;

  // Basic Localization Map
  static const Map<String, Map<String, String>> _localizedValues = {
    'en': {
      'home': 'Home',
      'catalog': 'Catalog',
      'account': 'Account',
      'settings': 'Settings',
      'cart': 'Cart',
      'login': 'Login',
      'logout': 'Logout',
      'my_account': 'My Account',
      'store_title': 'NopCommerce Store',
    },
    'fr': {
      'home': 'Accueil',
      'catalog': 'Catalogue',
      'account': 'Compte',
      'settings': 'Paramètres',
      'cart': 'Panier',
      'login': 'Se connecter',
      'logout': 'Se déconnecter',
      'my_account': 'Mon Compte',
      'store_title': 'Boutique NopCommerce',
    }
  };

  String translate(String key) {
    String langCode = _currentCulture.split('-')[0].toLowerCase();
    if (!_localizedValues.containsKey(langCode)) langCode = 'en';
    return _localizedValues[langCode]?[key] ?? key;
  }

  LanguageProvider() {
    _loadLanguage();
  }

  void setLanguage(Language language) {
    _currentLanguage = language;
    _currentLanguageId = language.id;
    _currentCulture = language.languageCulture;
    ApiService.languageId = language.id;
    _saveLanguage(language.id, language.languageCulture);
    notifyListeners();
  }

  _loadLanguage() async {
    final prefs = await SharedPreferences.getInstance();
    final langId = prefs.getInt('languageId');
    final langCulture = prefs.getString('languageCulture');
    
    if (langId != null) {
      _currentLanguageId = langId;
      ApiService.languageId = langId;
    }
    if (langCulture != null) {
      _currentCulture = langCulture;
    }
    notifyListeners();
  }

  _saveLanguage(int id, String culture) async {
    final prefs = await SharedPreferences.getInstance();
    prefs.setInt('languageId', id);
    prefs.setString('languageCulture', culture);
  }
}
