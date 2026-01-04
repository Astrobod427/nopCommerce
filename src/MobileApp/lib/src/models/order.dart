class Order {
  final int id;
  final String orderGuid;
  final int customerId;
  final int orderStatusId;
  final int paymentStatusId;
  final int shippingStatusId;
  final double orderTotal;
  final DateTime createdOnUtc;

  Order({
    required this.id,
    required this.orderGuid,
    required this.customerId,
    required this.orderStatusId,
    required this.paymentStatusId,
    required this.shippingStatusId,
    required this.orderTotal,
    required this.createdOnUtc,
  });

  factory Order.fromJson(Map<String, dynamic> json) {
    return Order(
      id: json['Id'],
      orderGuid: json['OrderGuid'],
      customerId: json['CustomerId'],
      orderStatusId: json['OrderStatusId'],
      paymentStatusId: json['PaymentStatusId'],
      shippingStatusId: json['ShippingStatusId'],
      orderTotal: (json['OrderTotal'] as num).toDouble(),
      createdOnUtc: DateTime.parse(json['CreatedOnUtc']),
    );
  }
}

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
