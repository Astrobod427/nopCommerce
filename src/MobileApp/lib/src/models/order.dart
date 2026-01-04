class OrderRequest {
  final int customerId;
  final List<OrderItemRequest> items;
  final BillingAddressRequest billingAddress;

  OrderRequest({
    required this.customerId,
    required this.items,
    required this.billingAddress,
  });

  Map<String, dynamic> toJson() {
    return {
      'CustomerId': customerId,
      'Items': items.map((i) => i.toJson()).toList(),
      'BillingAddress': billingAddress.toJson(),
    };
  }
}

class OrderItemRequest {
  final int productId;
  final int quantity;

  OrderItemRequest({
    required this.productId,
    required this.quantity,
  });

  Map<String, dynamic> toJson() {
    return {
      'ProductId': productId,
      'Quantity': quantity,
    };
  }
}

class BillingAddressRequest {
  final String firstName;
  final String lastName;
  final String email;
  final String city;
  final String address1;
  final String zipPostalCode;
  final String phoneNumber;
  final int countryId;

  BillingAddressRequest({
    required this.firstName,
    required this.lastName,
    required this.email,
    required this.city,
    required this.address1,
    required this.zipPostalCode,
    required this.phoneNumber,
    this.countryId = 1, // Default to 1 (usually USA or similar in default Nop)
  });

  Map<String, dynamic> toJson() {
    return {
      'FirstName': firstName,
      'LastName': lastName,
      'Email': email,
      'City': city,
      'Address1': address1,
      'ZipPostalCode': zipPostalCode,
      'PhoneNumber': phoneNumber,
      'CountryId': countryId,
    };
  }
}
