class Currency {
  final int id;
  final String name;
  final String currencyCode;
  final String displayLocale;
  final String customFormatting;
  final double rate;

  Currency({
    required this.id,
    required this.name,
    required this.currencyCode,
    required this.displayLocale,
    required this.customFormatting,
    required this.rate,
  });

  factory Currency.fromJson(Map<String, dynamic> json) {
    return Currency(
      id: json['Id'] ?? 0,
      name: json['Name'] ?? '',
      currencyCode: json['CurrencyCode'] ?? '',
      displayLocale: json['DisplayLocale'] ?? '',
      customFormatting: json['CustomFormatting'] ?? '',
      rate: (json['Rate'] as num?)?.toDouble() ?? 1.0,
    );
  }
}
