class Category {
  final int id;
  final String name;
  final String description;
  final String? imageUrl;

  Category({
    required this.id,
    required this.name,
    required this.description,
    this.imageUrl,
  });

  factory Category.fromJson(Map<String, dynamic> json) {
    return Category(
      id: json['Id'] ?? 0,
      name: json['Name'] ?? '',
      description: json['Description'] ?? '',
      imageUrl: json['ImageUrl'],
    );
  }
}
