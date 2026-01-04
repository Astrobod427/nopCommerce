import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:provider/provider.dart';
import 'src/config/theme.dart';
import 'src/screens/home_screen.dart';
import 'src/services/cart_provider.dart';
import 'src/services/auth_service.dart';
import 'src/services/theme_provider.dart';
import 'src/services/language_provider.dart';

Future<void> main() async {
  await dotenv.load(fileName: ".env");
  runApp(const NopCommerceApp());
}

class NopCommerceApp extends StatelessWidget {
  const NopCommerceApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => CartProvider()),
        ChangeNotifierProvider(create: (_) => AuthService()),
        ChangeNotifierProvider(create: (_) => ThemeProvider()),
        ChangeNotifierProvider(create: (_) => LanguageProvider()),
      ],
      child: Consumer<ThemeProvider>(
        builder: (context, themeProvider, child) {
          return MaterialApp(
            title: 'NopCommerce Mobile',
            debugShowCheckedModeBanner: false,
            theme: ThemeData(
              useMaterial3: true,
              colorScheme: ColorScheme.fromSeed(
                seedColor: themeProvider.primaryColor,
                primary: themeProvider.primaryColor,
                secondary: themeProvider.secondaryColor,
              ),
              appBarTheme: const AppBarTheme(
                centerTitle: true,
                elevation: 0,
              ),
              cardTheme: CardThemeData(
                elevation: 2,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              ),
            ),
            darkTheme: ThemeData.dark(useMaterial3: true).copyWith(
               colorScheme: ColorScheme.fromSeed(
                seedColor: themeProvider.primaryColor,
                primary: themeProvider.primaryColor,
                secondary: themeProvider.secondaryColor,
                brightness: Brightness.dark,
              ),
            ),
            themeMode: themeProvider.isDarkMode ? ThemeMode.dark : ThemeMode.light,
            home: const HomeScreen(),
          );
        },
      ),
    );
  }
}
