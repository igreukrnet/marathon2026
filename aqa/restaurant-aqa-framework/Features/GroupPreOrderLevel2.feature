Feature: Consolidated group pre-order confirmation

  After reviewing participant orders, the owner confirms the consolidated
  pre-order, combining identical dishes and submitting the final order to
  the restaurant.

  Background:
    Given the restaurant has a waiter account
    And the manager has set up a new location
    And the manager adds a main course dish to the location

  Rule: Successful confirmation consolidates orders and notifies the restaurant

    @level2 @ui @smoke
    Scenario: The owner successfully confirms the consolidated pre-order on the storefront
      Given the customer creates a group reservation
      And a guest called "Alex" joins the group and adds a dish
      And the guest "Alex" submits their cart
      And a guest called "Maria" joins the group and adds a dish
      And the guest "Maria" submits their cart
      When the owner opens the group pre-order page
      And the owner submits the consolidated pre-order
      Then the group pre-order is marked as sent to the restaurant

    @level2 @api
    Scenario: Unconfirmed carts are excluded and identical dishes are summed
      Given the customer creates a group reservation
      And a guest called "Alex" joins the group and adds 2 of the dish
      And the guest "Alex" submits their cart
      And a guest called "Maria" joins the group and adds 1 of the dish
      And the guest "Maria" submits their cart
      And a guest called "John" joins the group and adds 3 of the dish
      When the owner submits the consolidated pre-order
      Then the consolidated pre-order contains 3 of that dish
      And the order status is submitted by customer

    @level2 @api
    Scenario: The waiter sees the confirmed order with the consolidated dishes
      Given the customer creates a group reservation
      And a guest called "Alex" joins the group and adds a dish
      And the guest "Alex" submits their cart
      When the owner submits the consolidated pre-order
      Then the waiter sees the reservation order with status submitted by customer

  Rule: A consolidated pre-order can only be submitted under valid conditions

    @level2 @api @negative
    Scenario: Confirmation is rejected when no guest has submitted a cart
      Given the customer creates a group reservation
      And a guest called "Alex" joins the group and adds a dish
      When the owner attempts to submit the consolidated pre-order
      Then the submission is rejected because no guest carts were submitted

    @level2 @api @negative
    Scenario: Confirmation is rejected when submitted carts contain no dishes
      Given the customer creates a group reservation
      And a guest called "Alex" joins the group without adding dishes
      And the guest "Alex" submits their cart
      When the owner attempts to submit the consolidated pre-order
      Then the submission is rejected because no dishes were selected

    @level2 @api @negative
    Scenario: Confirming a non-existent group pre-order is rejected
      When a customer attempts to submit a pre-order for a non-existent group
      Then the group is reported as not found

    @level2 @api @negative
    Scenario: A customer who is not the owner cannot confirm the group pre-order
      Given the customer creates a group reservation
      And a guest called "Alex" joins the group and adds a dish
      And the guest "Alex" submits their cart
      When another customer attempts to submit the consolidated pre-order
      Then confirming the group pre-order is forbidden

    @level2 @api @negative
    Scenario: Submitting an already confirmed group pre-order is rejected
      Given the customer creates a group reservation
      And a guest called "Alex" joins the group and adds a dish
      And the guest "Alex" submits their cart
      And the owner submits the consolidated pre-order
      When the owner attempts to submit the consolidated pre-order again
      Then submitting the group pre-order again is rejected
