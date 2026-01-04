import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/cart_provider.dart';
import '../services/auth_service.dart';
import '../services/api_service.dart';
import '../models/order.dart';

class CheckoutScreen extends StatefulWidget {
  const CheckoutScreen({super.key});

  @override
  State<CheckoutScreen> createState() => _CheckoutScreenState();
}

class _CheckoutScreenState extends State<CheckoutScreen> {
  final _formKey = GlobalKey<FormState>();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _emailController = TextEditingController();
  final _cityController = TextEditingController();
  final _addressController = TextEditingController();
  final _zipController = TextEditingController();
  final _phoneController = TextEditingController();

  bool _isLoading = false;

  @override
  void initState() {
    super.initState();
    // Pre-fill from user profile if logged in
    final auth = Provider.of<AuthService>(context, listen: false);
    if (auth.isLoggedIn) {
      final user = auth.currentUser!;
      _firstNameController.text = user.firstName;
      _lastNameController.text = user.lastName;
      _emailController.text = user.email;
    }
  }

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _emailController.dispose();
    _cityController.dispose();
    _addressController.dispose();
    _zipController.dispose();
    _phoneController.dispose();
    super.dispose();
  }

  Future<void> _placeOrder() async {
    if (!_formKey.currentState!.validate()) return;

    final cart = Provider.of<CartProvider>(context, listen: false);
    final auth = Provider.of<AuthService>(context, listen: false);

    if (!auth.isLoggedIn) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please log in to place an order')),
      );
      return;
    }

    setState(() => _isLoading = true);

    try {
      final orderRequest = OrderRequest(
        customerId: auth.currentUser!.id,
        items: cart.items.map((item) => OrderItemRequest(
          productId: item.product.id,
          quantity: item.quantity,
        )).toList(),
        billingAddress: BillingAddressRequest(
          firstName: _firstNameController.text,
          lastName: _lastNameController.text,
          email: _emailController.text,
          city: _cityController.text,
          address1: _addressController.text,
          zipPostalCode: _zipController.text,
          phoneNumber: _phoneController.text,
        ),
      );

      await ApiService().createOrder(orderRequest);

      if (mounted) {
        cart.clear(); // Clear cart on success
        showDialog(
          context: context,
          barrierDismissible: false,
          builder: (context) => AlertDialog(
            title: const Text('Success'),
            content: const Text('Your order has been placed successfully!'),
            actions: [
              TextButton(
                onPressed: () {
                  Navigator.of(context).popUntil((route) => route.isFirst);
                },
                child: const Text('OK'),
              ),
            ],
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final cart = Provider.of<CartProvider>(context);

    return Scaffold(
      appBar: AppBar(title: const Text('Checkout')),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              padding: const EdgeInsets.all(16.0),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text(
                      'Order Summary',
                      style: Theme.of(context).textTheme.titleLarge,
                    ),
                    const Divider(),
                    ListView.builder(
                      shrinkWrap: true,
                      physics: const NeverScrollableScrollPhysics(),
                      itemCount: cart.items.length,
                      itemBuilder: (context, index) {
                        final item = cart.items[index];
                        return ListTile(
                          title: Text(item.product.name),
                          trailing: Text('${item.quantity} x \$${item.product.price}'),
                        );
                      },
                    ),
                    const Divider(),
                    Padding(
                      padding: const EdgeInsets.symmetric(vertical: 8.0),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          const Text('Total:', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 18)),
                          Text('\$${cart.totalAmount.toStringAsFixed(2)}', style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 18)),
                        ],
                      ),
                    ),
                    const SizedBox(height: 24),
                    Text(
                      'Shipping Address',
                      style: Theme.of(context).textTheme.titleLarge,
                    ),
                    const SizedBox(height: 16),
                    TextFormField(
                      controller: _firstNameController,
                      decoration: const InputDecoration(labelText: 'First Name'),
                      validator: (v) => v?.isEmpty ?? true ? 'Required' : null,
                    ),
                    TextFormField(
                      controller: _lastNameController,
                      decoration: const InputDecoration(labelText: 'Last Name'),
                      validator: (v) => v?.isEmpty ?? true ? 'Required' : null,
                    ),
                    TextFormField(
                      controller: _emailController,
                      decoration: const InputDecoration(labelText: 'Email'),
                      keyboardType: TextInputType.emailAddress,
                      validator: (v) => v?.isEmpty ?? true ? 'Required' : null,
                    ),
                    TextFormField(
                      controller: _addressController,
                      decoration: const InputDecoration(labelText: 'Address'),
                      validator: (v) => v?.isEmpty ?? true ? 'Required' : null,
                    ),
                    TextFormField(
                      controller: _cityController,
                      decoration: const InputDecoration(labelText: 'City'),
                      validator: (v) => v?.isEmpty ?? true ? 'Required' : null,
                    ),
                    TextFormField(
                      controller: _zipController,
                      decoration: const InputDecoration(labelText: 'Zip / Postal Code'),
                      validator: (v) => v?.isEmpty ?? true ? 'Required' : null,
                    ),
                    TextFormField(
                      controller: _phoneController,
                      decoration: const InputDecoration(labelText: 'Phone Number'),
                      keyboardType: TextInputType.phone,
                      validator: (v) => v?.isEmpty ?? true ? 'Required' : null,
                    ),
                    const SizedBox(height: 32),
                    ElevatedButton(
                      onPressed: _placeOrder,
                      style: ElevatedButton.styleFrom(
                        padding: const EdgeInsets.all(16),
                        backgroundColor: Theme.of(context).colorScheme.primary,
                        foregroundColor: Colors.white,
                      ),
                      child: const Text('Confirm & Place Order', style: TextStyle(fontSize: 18)),
                    ),
                  ],
                ),
              ),
            ),
    );
  }
}
