import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/theme_provider.dart';
import '../services/language_provider.dart';
import '../services/api_service.dart';
import '../models/language.dart';

class SettingsScreen extends StatefulWidget {
  const SettingsScreen({super.key});

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  final ApiService _apiService = ApiService();
  List<Language> _languages = [];
  bool _isLoadingLanguages = true;

  @override
  void initState() {
    super.initState();
    _loadLanguages();
  }

  Future<void> _loadLanguages() async {
    try {
      final langs = await _apiService.getLanguages();
      setState(() {
        _languages = langs;
        _isLoadingLanguages = false;
      });
    } catch (e) {
      setState(() {
        _isLoadingLanguages = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final themeProvider = Provider.of<ThemeProvider>(context);
    final languageProvider = Provider.of<LanguageProvider>(context);

    // Find current language object if ID is set
    Language? selectedLang;
    if (languageProvider.currentLanguageId > 0 && _languages.isNotEmpty) {
      try {
        selectedLang = _languages.firstWhere((l) => l.id == languageProvider.currentLanguageId);
      } catch (_) {}
    }

    return Scaffold(
      appBar: AppBar(title: const Text('Settings')),
      body: ListView(
        children: [
          ListTile(
            leading: const Icon(Icons.language),
            title: const Text('Language'),
            subtitle: Text(_isLoadingLanguages 
                ? 'Loading...' 
                : (selectedLang?.name ?? 'Select Language')),
            onTap: () {
              if (_languages.isNotEmpty) {
                _showLanguageDialog(languageProvider);
              }
            },
          ),
          SwitchListTile(
            secondary: const Icon(Icons.dark_mode),
            title: const Text('Dark Mode'),
            value: themeProvider.isDarkMode,
            onChanged: (value) {
              themeProvider.toggleTheme();
            },
          ),
          const Divider(),
          const ListTile(
            leading: const Icon(Icons.info),
            title: const Text('About'),
            subtitle: Text('Version 1.0.0'),
          ),
        ],
      ),
    );
  }

  void _showLanguageDialog(LanguageProvider languageProvider) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Select Language'),
        content: SizedBox(
          width: double.maxFinite,
          child: ListView.builder(
            shrinkWrap: true,
            itemCount: _languages.length,
            itemBuilder: (context, index) {
              final lang = _languages[index];
              return ListTile(
                title: Text(lang.name),
                leading: languageProvider.currentLanguageId == lang.id 
                    ? const Icon(Icons.check, color: Colors.blue) 
                    : null,
                onTap: () {
                  languageProvider.setLanguage(lang);
                  Navigator.pop(context);
                },
              );
            },
          ),
        ),
      ),
    );
  }
}
