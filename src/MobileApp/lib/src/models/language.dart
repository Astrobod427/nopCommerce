class Language {
  final int id;
  final String name;
  final String languageCulture;
  final String uniqueSeoCode;

  Language({
    required this.id,
    required this.name,
    required this.languageCulture,
    required this.uniqueSeoCode,
  });

  factory Language.fromJson(Map<String, dynamic> json) {
    return Language(
      id: json['Id'] ?? 0,
      name: json['Name'] ?? '',
      languageCulture: json['LanguageCulture'] ?? '',
      uniqueSeoCode: json['UniqueSeoCode'] ?? '',
    );
  }
}
