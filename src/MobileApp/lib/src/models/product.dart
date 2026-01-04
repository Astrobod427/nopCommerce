class Product {
  final int id;
  final String name;
  final String shortDescription;
  final String? fullDescription;
  final double price;
  final String? imageUrl;
  final List<String>? images;

  Product({
    required this.id,
    required this.name,
    required this.shortDescription,
    this.fullDescription,
    required this.price,
    this.imageUrl,
    this.images,
  });

  factory Product.fromJson(Map<String, dynamic> json) {
    return Product(
      id: json['Id'] ?? 0,
      name: json['Name'] ?? '',
      shortDescription: json['ShortDescription'] ?? '',
      fullDescription: json['FullDescription'],
      price: (json['Price'] ?? 0.0).toDouble(),
      imageUrl: json['ImageUrl'],
      images: json['Images'] != null ? List<String>.from(json['Images']) : null,
    );
  }
}
