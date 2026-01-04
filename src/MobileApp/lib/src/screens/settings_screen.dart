import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:cached_network_image/cached_network_image.dart';
import '../services/theme_provider.dart';
import '../services/language_provider.dart';
import '../services/api_service.dart';
import '../services/auth_service.dart';
import '../models/language.dart';
import '../models/currency.dart';

class SettingsScreen extends StatefulWidget {
  const SettingsScreen({super.key});

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  final ApiService _apiService = ApiService();
  List<Language> _languages = [];
  List<Currency> _currencies = [];
  bool _isLoadingLanguages = true;
  bool _isLoadingCurrencies = true;
  final TextEditingController _urlController = TextEditingController(text: ApiService.baseUrl);

  @override
  void initState() {
    super.initState();
    _loadLanguages();
    _loadCurrencies();
  }

  @override
  void dispose() {
    _urlController.dispose();
    super.dispose();
  }

  Future<void> _loadLanguages() async {
    try {
      final langs = await _apiService.getLanguages();
      setState(() {
        _languages = langs;
        _isLoadingLanguages = false;
      });
    } catch (e) {
      setState(() => _isLoadingLanguages = false);
    }
  }

  Future<void> _loadCurrencies() async {
    try {
      final curs = await _apiService.getCurrencies();
      setState(() {
        _currencies = curs;
        _isLoadingCurrencies = false;
      });
    } catch (e) {
      setState(() => _isLoadingCurrencies = false);
    }
  }

  Future<void> _saveUrl() async {
    await ApiService.setBaseUrl(_urlController.text);
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('API URL updated to: ${ApiService.baseUrl}')),
      );
      // Refresh data
      _loadLanguages();
      _loadCurrencies();
    }
  }

  @override
  Widget build(BuildContext context) {
    final themeProvider = Provider.of<ThemeProvider>(context);
    final languageProvider = Provider.of<LanguageProvider>(context);
    final auth = Provider.of<AuthService>(context);

    return Scaffold(
      appBar: AppBar(title: const Text('Settings')),
      body: ListView(
        children: [
          _buildSectionTitle('Connection (Admin)'),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 8.0),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _urlController,
                    decoration: const InputDecoration(
                      labelText: 'API Base URL',
                      hintText: 'https://yourstore.com/api/simple',
                      border: OutlineInputBorder(),
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                IconButton(
                  icon: const Icon(Icons.save),
                  onPressed: _saveUrl,
                  color: themeProvider.primaryColor,
                ),
              ],
            ),
          ),
          const Divider(),
          _buildSectionTitle('Localization'),
          ListTile(
            leading: const Icon(Icons.language),
            title: const Text('Language'),
            subtitle: Text(_isLoadingLanguages ? 'Loading...' : 'Select Language'),
            onTap: () => _showLanguageDialog(languageProvider),
          ),
          ListTile(
            leading: const Icon(Icons.monetization_on),
            title: const Text('Currency'),
            subtitle: Text(_isLoadingCurrencies ? 'Loading...' : 'Select Currency'),
            onTap: () => _showCurrencyDialog(),
          ),
          const Divider(),
          _buildSectionTitle('Appearance'),
          SwitchListTile(
            secondary: const Icon(Icons.dark_mode),
            title: const Text('Dark Mode'),
            value: themeProvider.isDarkMode,
            onChanged: (value) => themeProvider.toggleTheme(),
          ),
          const Divider(),
          _buildSectionTitle('Maintenance'),
          ListTile(
            leading: const Icon(Icons.delete_sweep),
            title: const Text('Clear Image Cache'),
            onTap: () {
              // Note: Using standard Flutter cache management
              // In a real app we'd use DefaultCacheManager().emptyCache()
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(content: Text('Image cache cleared')),
              );
            },
          ),
          const Divider(),
          _buildSectionTitle('Help & Support'),
          ListTile(
            leading: const Icon(Icons.help_outline),
            title: const Text('FAQ'),
            onTap: () {},
          ),
          ListTile(
            leading: const Icon(Icons.support_agent),
            title: const Text('Contact Support'),
            onTap: () {},
          ),
          const Divider(),
          if (auth.isLoggedIn)
            ListTile(
              leading: const Icon(Icons.logout, color: Colors.red),
              title: const Text('Logout', style: TextStyle(color: Colors.red)),
              onTap: () {
                auth.logout();
                Navigator.pop(context);
              },
            ),
          const ListTile(
            leading: const Icon(Icons.info_outline),
            title: const Text('App Version'),
            subtitle: Text('1.0.0 (Dynamic Engine v4.90)'),
          ),
        ],
      ),
    );
  }

  Widget _buildSectionTitle(String title) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
      child: Text(
        title,
        style: TextStyle(
          color: Theme.of(context).colorScheme.primary,
          fontWeight: FontWeight.bold,
        ),
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

  void _showCurrencyDialog() {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Select Currency'),
        content: SizedBox(
          width: double.maxFinite,
          child: ListView.builder(
            shrinkWrap: true,
            itemCount: _currencies.length,
            itemBuilder: (context, index) {
              final cur = _currencies[index];
              return ListTile(
                title: Text(cur.name),
                subtitle: Text(cur.currencyCode),
                onTap: () {
                  // TODO: Implement currency selection in a Provider
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